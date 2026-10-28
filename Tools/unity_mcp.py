"""Petit client du serveur MCP de Unity (HTTP, 127.0.0.1:8080/mcp), quand la session Claude a perdu sa connexion.
Utilisation : python3 unity.py <outil> '<arguments JSON>'   (ex. execute_code '{"action":"execute","code":"return 1;"}')"""
import json, sys, urllib.request

URL = "http://127.0.0.1:8080/mcp"
H = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}


def poster(corps, session=None):
    h = dict(H)
    if session: h["Mcp-Session-Id"] = session
    req = urllib.request.Request(URL, data=json.dumps(corps).encode(), headers=h)
    with urllib.request.urlopen(req, timeout=300) as r:
        sid = r.headers.get("Mcp-Session-Id") or session
        brut = r.read().decode()
    msgs = []
    for ligne in brut.splitlines():
        if ligne.startswith("data:"):
            try: msgs.append(json.loads(ligne[5:].strip()))
            except ValueError: pass
    if not msgs and brut.strip():
        try: msgs.append(json.loads(brut))
        except ValueError: pass
    return sid, msgs


sid, _ = poster({"jsonrpc": "2.0", "id": 1, "method": "initialize",
                 "params": {"protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "claude-secours", "version": "1"}}})
try: poster({"jsonrpc": "2.0", "method": "notifications/initialized"}, sid)
except Exception: pass
outil, args = sys.argv[1], json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
_, msgs = poster({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {"name": outil, "arguments": args}}, sid)
for m in msgs:
    res = m.get("result", m.get("error"))
    for c in (res or {}).get("content", []) if isinstance(res, dict) else []:
        if c.get("type") == "text": print(c["text"][:4000])
        elif c.get("type") == "image": print("[image", c.get("mimeType"), len(c.get("data", "")), "octets base64]")
    if isinstance(res, dict) and "content" not in res: print(json.dumps(res)[:4000])
