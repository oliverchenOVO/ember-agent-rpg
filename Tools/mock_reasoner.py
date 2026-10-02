"""Loopback-only optional gateway test: valid response, malformed response, timeout."""
import json,time,threading
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
log=[];lock=threading.Lock()
class Handler(BaseHTTPRequestHandler):
 def log_message(self,*args):pass
 def do_POST(self):
  request=json.loads(self.rfile.read(int(self.headers['Content-Length'])))
  with lock:
   number=len(log)+1;log.append(dict(number=number,agent=request['context']['agent'],phase=request['context']['phase']))
   (ROOT/'Artifacts/mock-reasoner.json').write_text(json.dumps(log),encoding='utf-8')
  if number%3==0:time.sleep(2.3)
  context=request['context']
  response=dict(intent='Fight' if context['phase']==0 else 'Exit',targetGoal='advance',reasoningTags=['gateway_fixture'],riskLevel=.3,proposedAction='',dialogueIntent='',confidence=.8)
  raw=(b'{invalid' if number%3==2 else json.dumps(response).encode())
  try:self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(raw)));self.end_headers();self.wfile.write(raw)
  except (BrokenPipeError,ConnectionResetError):pass
if __name__=='__main__':ThreadingHTTPServer(('127.0.0.1',8766),Handler).serve_forever()
