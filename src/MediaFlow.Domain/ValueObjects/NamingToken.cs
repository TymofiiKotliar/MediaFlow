namespace MediaFlow.Domain.ValueObjects;

public abstract record NamingToken;

public sealed record PrefixToken(string Text) : NamingToken;
public sealed record SequenceNumberToken : NamingToken;
public sealed record CurrentDateToken : NamingToken;
public sealed record PhotoDateToken : NamingToken;
