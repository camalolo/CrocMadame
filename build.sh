#!/usr/bin/bash
dotnet publish -c Release --self-contained true --runtime win-x64 -p:PublishSingleFile=true
