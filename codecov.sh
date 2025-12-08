#!/usr/bin/env bash

TOKEN=$1
rm -r ./basics/timelock-contract/test/TestResults
rm -r CodeCoverage
for name in `ls ./basics/timelock-contract/test/*.csproj | awk '{print $NF}'`;
do
    echo ${name}
    dotnet test ${name} --logger trx --settings CodeCoverage.runsettings --collect:"XPlat Code Coverage"
done
reportgenerator /basics/timelock-contract/test/TestResults/*/coverage.cobertura.xml -reports:./basics/timelock-contract/test/TestResults/*/coverage.cobertura.xml -targetdir:./CodeCoverage -reporttypes:Cobertura -assemblyfilters:-xunit*
codecov -f ./CodeCoverage/Cobertura.xml -t ${TOKEN}