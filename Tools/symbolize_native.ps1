param([string]$Image,[string]$Symbols,[string]$Output,[string]$BaseHex="",[switch]$Scan,[string]$Mask="*ThreadsafeLinearAllocator*",[string]$LogPath='')
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public static class EmberSymbols {
 [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
 [DllImport("dbghelp.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool SymInitializeW(IntPtr p,string path,bool invade);
 [DllImport("dbghelp.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern ulong SymLoadModuleExW(IntPtr p,IntPtr file,string image,string module,ulong address,uint size,IntPtr data,uint flags);
 [DllImport("dbghelp.dll")] static extern bool SymCleanup(IntPtr p);
 [DllImport("dbghelp.dll",SetLastError=true)] static extern bool SymFromAddr(IntPtr p,ulong address,out ulong displacement,IntPtr info);
 [DllImport("dbghelp.dll",SetLastError=true,CharSet=CharSet.Ansi)] static extern bool SymEnumSymbols(IntPtr p,ulong module,string mask,Callback cb,IntPtr user);
 delegate bool Callback(IntPtr info,uint size,IntPtr user);
 static IntPtr process;static ulong loaded;
 public static void Open(string image,string path){process=GetCurrentProcess();if(!SymInitializeW(process,path,false))throw new Exception("SymInitialize "+Marshal.GetLastWin32Error());loaded=SymLoadModuleExW(process,IntPtr.Zero,image,"UnityPlayer",0x180000000,0,IntPtr.Zero,0);if(loaded==0)throw new Exception("SymLoad "+Marshal.GetLastWin32Error());}
 public static string[] Matching(string mask){var result=new List<string>();Callback cb=(info,size,user)=>{ulong address=(ulong)Marshal.ReadInt64(info,56);string name=Marshal.PtrToStringAnsi(IntPtr.Add(info,84),Marshal.ReadInt32(info,76));result.Add((address-loaded).ToString("x")+" "+name);return true;};SymEnumSymbols(process,loaded,mask,cb,IntPtr.Zero);GC.KeepAlive(cb);return result.ToArray();}
 public static string At(ulong rva){IntPtr info=Marshal.AllocHGlobal(4096);try{for(int i=0;i<88;i++)Marshal.WriteByte(info,i,0);Marshal.WriteInt32(info,0,88);Marshal.WriteInt32(info,80,1024);ulong delta;if(!SymFromAddr(process,loaded+rva,out delta,info))return "unresolved";return Marshal.PtrToStringAnsi(IntPtr.Add(info,84),Marshal.ReadInt32(info,76))+" + 0x"+delta.ToString("x");}finally{Marshal.FreeHGlobal(info);}}
 public static void Close(){SymCleanup(process);}
}
'@
try {
 [EmberSymbols]::Open($Image,$Symbols)
 if($LogPath){
  if(-not $BaseHex){throw 'Log symbolication requires a separately verified module base.'}
  [ulong]$taskBase=[Convert]::ToUInt64($BaseHex,16)
  $taskAddresses=[regex]::Matches([IO.File]::ReadAllText([IO.Path]::GetFullPath($LogPath)),'0x([0-9a-fA-F]+) \(UnityPlayer\)') | ForEach-Object {$_.Groups[1].Value} | Select-Object -Unique
  $taskLines=$taskAddresses | ForEach-Object {$_+' '+[EmberSymbols]::At([Convert]::ToUInt64($_,16)-$taskBase)}
 }
 elseif($Scan){$taskLines=@();for([ulong]$taskBase=0x7ffb6a000000;$taskBase -le 0x7ffb6bcb0000;$taskBase+=0x10000){$taskLines+=($taskBase.ToString('x')+' '+[EmberSymbols]::At(0x7ffb6c70a7ad-$taskBase))}}
 elseif($BaseHex){[ulong]$taskBase=[Convert]::ToUInt64($BaseHex,16);$taskAddresses=@(0x7ffb6c70a7ad,0x7ffb6c70a93c,0x7ffb6ca56226,0x7ffb6d8daa53,0x7ffb6bcf73c2,0x7ffb6bcbeded,0x7ffb6bcc59d3,0x7ffb6bd14156,0x7ffb6c2bb96f,0x7ffb6c2b7ba2,0x7ffb6c2998b8,0x7ffb6c299974,0x7ffb6c29fe7a,0x7ffb6c865741,0x7ffb6c86469d,0x7ffb6c869319,0x7ffb6c86a0ab);$taskLines=$taskAddresses | ForEach-Object { $_.ToString('x')+' '+[EmberSymbols]::At($_-$taskBase) }}
 else{$taskLines=[EmberSymbols]::Matching($Mask)}
 $taskLines | Set-Content -LiteralPath $Output -Encoding utf8
 Write-Output ("Symbols saved: "+$taskLines.Count)
} finally { [EmberSymbols]::Close() }
