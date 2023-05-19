#!/usr/bin/bash

IFS='}' read -ra testplans <<< $1     # separate the parameters to testplans
cd ../../src/PowerAppsTestEngine
for testplan in "${testplans[@]}" 
do
  IFS=',' read -ra args <<< "$testplan"                    # separate the testplan arguments to args
  envId=''
  tenantId=''
  domain=''
  testPlanFile=''
  outputDir=''
  for arg in "${args[@]}" 
    do      
      if [[ $arg == *"environmentId"* ]]; then
        IFS=':' read -ra value <<< "$arg"
        if [ -n "${value[1]}" ]; then
          envId=${value[1]}
        fi
      fi
      if [[ $arg == *"tenantId"* ]]; then
        IFS=':' read -ra value <<< "$arg" 
        if [ -n "${value[1]}" ]; then
          tenantId=${value[1]}
        fi
      fi
      if [[ $arg == *"domain"* ]]; then
        IFS=':' read -ra value <<< "$arg"
        if [ -n "${value[1]}" ]; then
          domain=${value[1]}
        fi
      fi
      if [[ $arg == *"testPlanFile"* ]]; then
        IFS=':' read -ra value <<< "$arg"
        if [ -n "${value[1]}" ]; then
          testPlanFile=${value[1]}
        fi
      fi
      if [[ $arg == *"outputDirectory"* ]]; then
        IFS=':' read -ra value <<< "$arg"
        if [ -n "${value[1]}" ]; then
          outputDir=${value[1]}
        fi
       fi                                                                
    done
  if [[ -n "${envId}" && -n "${tenantId}" && -n "${domain}" && -n "${testPlanFile}" && -n "${outputDir}" ]]; then     # null checks on args
    dotnet run -f net6.0 -- -e ${envId} -t ${tenantId} -d ${domain} -i ${testPlanFile} -o ${outputDir} -q "&PAOverrideFGRollout.OnePlayerStandaloneWebPlayer=false";
    dotnet run -f net6.0 -- -e ${envId} -t ${tenantId} -d ${domain} -i ${testPlanFile} -o ${outputDir} -q "&PAOverrideFGRollout.OnePlayerStandaloneWebPlayer=true";
  fi
done