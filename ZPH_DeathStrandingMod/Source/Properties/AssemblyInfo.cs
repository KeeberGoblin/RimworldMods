using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Death Stranding Mod")]
[assembly: AssemblyDescription("A comprehensive Death Stranding total conversion for RimWorld featuring timefall weather, BT creatures, DOOMS abilities, chiral technology, and voidout events.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Death Stranding for RimWorld")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components. If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Additional assembly attributes for mod identification
[assembly: AssemblyMetadata("ModName", "Death Stranding")]
[assembly: AssemblyMetadata("Author", "YourName")]
[assembly: AssemblyMetadata("RimWorldVersion", "1.6")]
[assembly: AssemblyMetadata("ModVersion", "1.0.0")]
[assembly: AssemblyMetadata("Description", "Keep on keeping on.")]

// Mark assembly as compatible with RimWorld modding
[assembly: AssemblyMetadata("RimWorldCompatible", "true")]
[assembly: AssemblyMetadata("RequiredDLCs", "None")]
[assembly: AssemblyMetadata("OptionalDLCs", "Biotech,Royalty,Ideology,Anomaly")]

// Security and performance attributes
[assembly: System.Security.AllowPartiallyTrustedCallers]
[assembly: System.Runtime.CompilerServices.CompilationRelaxations(CompilationRelaxations.NoStringInterning)]

// Assembly loading optimization
[assembly: AssemblyMetadata("PreferredLoadContext", "Default")]
[assembly: AssemblyMetadata("ModLoadPriority", "Normal")]

// Debugging information
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: System.Diagnostics.Debuggable(System.Diagnostics.DebuggableAttribute.DebuggingModes.Default | 
                                         System.Diagnostics.DebuggableAttribute.DebuggingModes.DisableOptimizations)]
#else
[assembly: AssemblyConfiguration("Release")]
#endif