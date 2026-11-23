# Checker for scripts/readonly-bash.sh. Reads the command from $COMMAND.
# Prints the reason when the command is not read-only; prints nothing when it is.
import os, shlex

ALLOWED = {
    "rg", "grep", "egrep", "fgrep", "ls", "cat", "head", "tail", "wc", "sort", "uniq", "cut", "tr",
    "echo", "printf", "test", "[", "jq", "nl", "basename", "dirname", "diff", "file", "stat", "pwd",
    "cd", "true", "false", "find", "sed", "git",
}
GIT_ALLOWED = {
    "diff", "log", "show", "status", "grep", "rev-parse", "merge-base", "ls-files", "ls-tree",
    "blame", "cat-file", "describe", "name-rev", "shortlog", "rev-list", "diff-tree", "show-ref",
}
FIND_DENIED = {"-delete", "-exec", "-execdir", "-ok", "-okdir", "-fprint", "-fprintf", "-fls"}

raw = os.environ["COMMAND"]
for needle in ("$(", chr(96), "<(", ">("):
    if needle in raw:
        print("command substitution is not allowed")
        raise SystemExit

lex = shlex.shlex(os.environ["COMMAND"], posix=True, punctuation_chars=True)
lex.whitespace_split = True
try:
    tokens = list(lex)
except ValueError as e:
    print(f"could not parse the command ({e})")
    raise SystemExit

def deny(msg):
    print(msg)
    raise SystemExit

segments, cur = [], []
for tok in tokens:
    if tok and set(tok) <= set("|&;"):
        if cur:
            segments.append(cur)
        cur = []
    else:
        cur.append(tok)
if cur:
    segments.append(cur)

for seg in segments:
    # drop VAR=value prefixes
    while seg and "=" in seg[0] and not seg[0].startswith("-") and seg[0].split("=")[0].isidentifier():
        seg = seg[1:]
    if not seg:
        continue
    for i, tok in enumerate(seg):
        if "$(" in tok or "`" in tok or "<(" in tok:
            deny("command substitution is not allowed")
        if set(tok) <= set("<>&") and ">" in tok:
            nxt = seg[i + 1] if i + 1 < len(seg) else ""
            if not (nxt == "/dev/null" or (tok.endswith("&") and nxt.isdigit()) or tok == ">&"):
                deny("output redirection is not allowed")
    name = seg[0].rsplit("/", 1)[-1]
    if name not in ALLOWED:
        deny(f"'{name}' is not a read-only command")
    if name == "git":
        sub, skip = "", False
        for a in seg[1:]:
            if skip:
                skip = False
            elif a in ("-C", "-c", "--git-dir", "--work-tree", "--namespace"):
                skip = True
            elif not a.startswith("-"):
                sub = a
                break
        if sub not in GIT_ALLOWED:
            deny(f"'git {sub}' is not allowed")
    if name == "find" and FIND_DENIED & set(seg):
        deny("find with -delete, -exec or -fprint is not allowed")
    if name == "sed" and any(a == "-i" or a.startswith("-i") or a.startswith("--in-place") for a in seg[1:]):
        deny("sed -i is not allowed")
