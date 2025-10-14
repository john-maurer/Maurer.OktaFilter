#docker run --rm -v "${PWD}:/src" -w /src oktafilter-build

FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /src
RUN dotnet new tool-manifest
RUN dotnet tool install Cake.Tool
RUN dotnet --info
CMD ["bash","-lc","dotnet tool restore && dotnet cake --target=Pack --skipIntegration=true --semver 0.0.0-local"]