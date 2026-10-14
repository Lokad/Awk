# Security and host containment

Lokad.Awk runs managed code and routes all files, descriptors, environment data and
commands through the embedding application's `IAwkHost`. Awk code has no direct .NET
filesystem, process, console, networking, reflection or native-loading capability.
The host and its dependencies remain trusted; this boundary is not an OS sandbox.

## Configure authority

Use `AwkExecutionPolicy.CreateRestricted()` for untrusted scripts. It disables
script-created redirections and pipes, `@include` and changed `ARGV` input paths;
hides environment and host-error details; fixes time to the Unix epoch/UTC; and
applies finite data/memory budgets, host deadlines and a cooperative 30-second deadline.
Explicit invocation inputs remain available through the host.

`--sandbox` only disables file/command redirections. It does not replace the restricted
preset or host policy. `AccessiblePathRoot` checks canonical virtual paths lexically;
a filesystem host must enforce its own root, including symlinks, reparse points and races.
Standard `/dev/*` paths refer to supplied descriptors, not ambient operating-system files.

## Implement the host boundary

Return structured missing, unsupported, failure and exit results. Unexpected host/framework
exceptions become generic awk diagnostics without CLR exception text. Caller cancellation
propagates to the application as cancellation. Follow the read-buffer lifetime and outcome
contract in the [embedding guide](../README.md#embedding).

With a shared memory budget, cleanup awaits actual host users before refunding their
storage. Hosts must cooperate with cancellation; a deadline ends a wait but does not
prove that the underlying task has stopped using buffers.

One-way pipes require both policy and host permission. Their complete command text is
passed as an opaque command identity; the runtime does not tokenize or invoke a shell.
A host should resolve allowed commands, build arguments without shell parsing, control
working directory/environment and bound execution/output. `system()`, coprocesses,
network special files and dynamic/native extensions are unsupported.

## Bound resource use

Both presets have no implicit execution fuel. Supply caller cancellation or an owned
deadline, and choose memory/data limits appropriate to the workload. Mutation preflights
preserve prior state/output when resource admission fails. See [runtime limits](RUNTIME_LIMITS.md).

Memory is a conservative logical runtime budget, not a total-process heap cap.
Cancellation is cooperative: dependency calls can finish before it is observed, and a
timed-out host task can continue effects if the host ignores cancellation. Host and cleanup
deadlines bound awaiting, not forced termination. Use OS process/container limits when
untrusted workloads require hard availability guarantees.

Ingress stays [strict UTF-8](UTF8_POLICY.md). The library disables unsafe C#; parser and
regex dependencies remain part of the trusted implementation and require qualification
when updated.
