using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class ReasonerRecord
    {
        public int agent,run,revision,attempt; public string status,contextJson,decisionJson;
    }
    [Serializable] public class LLMRequest
    {
        public string schema="ember.high-decision.v1"; public int maxOutputTokens=256;
        public AgentDecisionContext context;
        public string[] allowedIntents;
        public string instructions="Return only HighDecision JSON. intent must be an allowed Goal; proposedAction must be an allowed site ID. No commands, transforms, animation or code. reasoningTags <=8; confidence/riskLevel 0..1.";
    }
    public interface IReasonerTransport { Task<string> SendAsync(string request,CancellationToken cancellation); }
    // Provider-neutral JSON decision gateway. No provider SDK or secret is part of the save.
    public sealed class HttpReasonerTransport : IReasonerTransport,IDisposable
    {
        readonly HttpClient client=new HttpClient(); readonly Uri endpoint; readonly string key;
        public HttpReasonerTransport(string endpoint,string key)
        {
            this.endpoint=new Uri(endpoint);if(this.endpoint.Scheme!="https"&&!this.endpoint.IsLoopback)throw new ArgumentException("Remote reasoner requires HTTPS");this.key=key;client.Timeout=Timeout.InfiniteTimeSpan;
        }
        public async Task<string> SendAsync(string request,CancellationToken cancellation)
        {
            using(var message=new HttpRequestMessage(HttpMethod.Post,endpoint))
            {
                if(!string.IsNullOrEmpty(key))message.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",key);
                message.Content=new StringContent(request,Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(message,HttpCompletionOption.ResponseContentRead,cancellation).ConfigureAwait(false))
                {response.EnsureSuccessStatusCode();string text=await response.Content.ReadAsStringAsync().ConfigureAwait(false);if(text.Length>16384)throw new InvalidOperationException("Response too large");return text;}
            }
        }
        public void Dispose()=>client.Dispose();
    }
    public sealed class LLMReasoner : IAgentReasoner
    {
        readonly IReasonerTransport transport;readonly RuleBasedReasoner fallback=new RuleBasedReasoner();
        readonly object gate=new object();readonly Stopwatch clock=Stopwatch.StartNew();double nextRequest;
        public int timeoutMs=1800,maxRetries=1,maxRequests=64,requestTokenBudget=8192,runTokenBudget=65536;public double minimumIntervalSeconds=2;
        public int requests,tokensReserved; public readonly List<ReasonerRecord> records=new List<ReasonerRecord>();
        public LLMReasoner(IReasonerTransport transport){this.transport=transport;}
        public List<ReasonerRecord> Snapshot(){lock(gate)return new List<ReasonerRecord>(records);}
        public void ResetBudget(){lock(gate){requests=0;tokensReserved=0;nextRequest=0;}}
        void Record(AgentDecisionContext c,int attempt,string status,string request,HighDecision decision)
        {
            lock(gate){records.Add(new ReasonerRecord{agent=c.agent,run=c.run,revision=c.revision,attempt=attempt,status=status,contextJson=request,decisionJson=JsonUtility.ToJson(decision)});if(records.Count>128)records.RemoveAt(0);}
        }
        public async Task<HighDecision> DecideAsync(AgentDecisionContext c,CancellationToken cancellation)
        {
            string request=JsonUtility.ToJson(new LLMRequest{context=c,allowedIntents=c.phase==Phase.Battle?new[]{"Fight","Support","Rescue"}:new[]{"Support","Recover","Forge","Intel","ReadBook","Loot","Exit","Rescue","LeaveParty","Rejoin"}});
            // Conservative reservation includes input and bounded output; gateway must enforce its own tokenizer too.
            int reserve=Encoding.UTF8.GetByteCount(request)+256;
            for(int attempt=0;attempt<=maxRetries;attempt++)
            {
                bool allowed;
                lock(gate){allowed=transport!=null&&reserve<=requestTokenBudget&&tokensReserved+reserve<=runTokenBudget&&requests<maxRequests&&clock.Elapsed.TotalSeconds>=nextRequest;
                    if(allowed){requests++;tokensReserved+=reserve;nextRequest=clock.Elapsed.TotalSeconds+minimumIntervalSeconds;}}
                if(!allowed){var limited=fallback.Decide(c);limited.provider="fallback";Record(c,attempt,"budget_or_rate_limit",request,limited);return limited;}
                string failure="invalid";
                using(var linked=CancellationTokenSource.CreateLinkedTokenSource(cancellation))
                {
                    try
                    {
                        var pending=transport.SendAsync(request,linked.Token);var timeout=Task.Delay(timeoutMs,cancellation);
                        if(await Task.WhenAny(pending,timeout).ConfigureAwait(false)!=pending)
                        {
                            linked.Cancel();failure="timeout";
                            // Observe a late fault without ever blocking the simulation or accepting a late response.
                            _=pending.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
                        }
                        else
                        {
                            string raw=await pending.ConfigureAwait(false);
                            if(raw==null||raw.Length>16384||!raw.TrimStart().StartsWith("{"))throw new InvalidOperationException("Invalid JSON");
                            var decision=new HighDecision{intent=null,targetGoal=null,proposedAction=null,dialogueIntent=null,reasoningTags=null,riskLevel=float.NaN,confidence=float.NaN};
                            JsonUtility.FromJsonOverwrite(raw,decision);
                            if(!AgentDecisionContext.Validate(decision,c))throw new InvalidOperationException("Schema violation");
                            decision.provider="llm";Record(c,attempt,"accepted",request,decision);return decision;
                        }
                    }
                    catch(OperationCanceledException){failure="cancelled";}
                    catch{failure="transport_or_schema_error";}
                    var local=fallback.Decide(c);local.provider="fallback";Record(c,attempt,failure,request,local);
                    if(attempt==maxRetries||cancellation.IsCancellationRequested)return local;
                }
                // Retry remains bounded and obeys the same rate limiter.
                if(attempt<maxRetries&&minimumIntervalSeconds>0)
                {try{await Task.Delay((int)(minimumIntervalSeconds*1000),cancellation).ConfigureAwait(false);}catch(OperationCanceledException){return fallback.Decide(c);}}
            }
            return fallback.Decide(c);
        }
        public static HighDecision Replay(ReasonerRecord record,AgentDecisionContext context)
        {
            if(record.run!=context.run||record.agent!=context.agent||record.revision!=context.revision)return new RuleBasedReasoner().Decide(context);
            var d=JsonUtility.FromJson<HighDecision>(record.decisionJson);return AgentDecisionContext.Validate(d,context)?d:new RuleBasedReasoner().Decide(context);
        }
    }
}
