# il2cpp-dll2sdk
generate a c++ SDK from a dummy dll generated by https://github.com/Perfare/Il2CppDumper

# how 2 use
this won't work out of the box, you need to do a bit of config
find your il2cpp version, include the relevant bits from here: 
https://github.com/Perfare/Il2CppDumper/blob/master/Il2CppDumper/Outputs/HeaderConstants.cs
then compile and you should be good to go

1. dump dummy dlls with il2cppdumper
2. run dll2sdk with the folder as the first argument
3. profit

# stuff not supported
1. static fields in structures
2. generic packed structures

# assumptions made
1. all fields in classes are made in the order that they appear in the actual binary
2. all methods have an "Address" attribute with a string field called "RVA" corresponding to the RVA
3. virtual methods have a "Slot" field set in the "Address" attribute
4. packed structures have a "FieldOffset" field with a string that corresponds to the hexadecimal offset of the field

if your dumper does that then it should work with this
