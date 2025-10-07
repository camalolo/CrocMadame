#!/usr/bin/bash
#!/usr/bin/bash
dotnet publish -c Release --self-contained true --runtime win-x64 \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableUnsafeBinaryFormatterSerialization=false \
  -p:EnableUnsafeUTF8Strings=false