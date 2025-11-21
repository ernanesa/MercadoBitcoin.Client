using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Generated code uses reflection-based JSON serialization, but we provide a source-generated context at runtime.", Scope = "namespaceanddescendants", Target = "MercadoBitcoin.Client.Generated")]
[assembly: SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to target method. The return value of the source method does not have matching annotations.", Justification = "Generated code uses reflection, but we provide a source-generated context at runtime.", Scope = "namespaceanddescendants", Target = "MercadoBitcoin.Client.Generated")]
