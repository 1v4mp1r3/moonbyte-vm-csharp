# Security Policy

MoonByte is an educational interpreter and embedding lab. Scripts cannot access files, processes, networking, assemblies, native code or environment variables unless the host application exposes such capabilities through registered host functions.

Do not use MoonByte to run untrusted scripts in production without additional sandboxing, resource limits and host API review. The default VM has a step limit, but that is not a complete security boundary.

Reports about runtime crashes, unsafe host API examples, parser bugs or documentation mistakes are welcome.

