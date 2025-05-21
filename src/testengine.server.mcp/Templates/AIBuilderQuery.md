# Recommendation 

Use the PowerShell to query the available AI Builder models 

## Variables

If variables in the format {{name}} exist in the recommendation try read the values from the tests\variables.yaml or context from the workspace.

If a tests\variables.yaml file does not exist query the Test Engine MCP Server to the "variables.yaml" template

## Recommendations

For the template

- Summarize what this template does for a MCP Client user
- Ensure that create PowerShell based on this query
- The should MUST use the msdyn_aimodels entity once the solution id is found

### AI Builder Query

- Direct query for AI Builder models by solution ID:

    ```PowerShell
    $solutionName = '{{solution.yaml\SolutionManifest\description}}'
    # Get the environment URL using a simple filter
    Write-Host "Fetching available environments..." -ForegroundColor Cyan
    try {
        # Get filter from user
        $filter = Read-Host "Enter environment filter (e.g. 'dev', 'prod', etc.)"
        
        # If empty, use active environment
        if ([string]::IsNullOrWhiteSpace($filter)) {
            Write-Host "No filter provided." -ForegroundColor Red
            exit 1
        } else {
            # Get environments using filter
            $environments = & pac env list --json | ConvertFrom-Json
            $filteredEnvs = $environments | Where-Object { $_.FriendlyName -like "*$filter*" }
            
            if ($filteredEnvs.Length -eq 0) {
                Write-Host "No environments match the filter: $filter" -ForegroundColor Red
                exit 1
            }
            
            # Assume there should be only one match
            $selectedEnv = $filteredEnvs[0]
            
            # Check if there are multiple matches and warn the user
            if ($filteredEnvs.Length -gt 1) {
                Write-Host "Warning: Multiple environments match your filter. Using the first match." -ForegroundColor Yellow
                Write-Host "Selected: $($selectedEnv.FriendlyName)" -ForegroundColor Yellow
                Write-Host "To use a different environment, restart the script with a more specific filter." -ForegroundColor Yellow
            }
        }
        
        # Set the environment URL
        $environmentUrl = $selectedEnv.EnvironmentUrl
        if (!$environmentUrl.EndsWith('/')) {
            $environmentUrl += '/'
        }
        Write-Host "`nUsing environment: $($selectedEnv.FriendlyName) - $environmentUrl" -ForegroundColor Green
        
    } catch {
        Write-Host "Error getting environment URL: $_" -ForegroundColor Red
        exit 1
    }

    Write-Host "Getting access token..." -ForegroundColor Cyan
    $tokenInfo = az account get-access-token --resource $environmentUrl | ConvertFrom-Json
    $bearerToken = $tokenInfo.accessToken
    $headers = @{
        "Authorization" = "Bearer $bearerToken"
        "Accept" = "application/json"
        "OData-MaxVersion" = "4.0"
        "OData-Version" = "4.0"
    }

    # Step 1: Get the solution ID using the solution name
    Write-Host "Getting solution ID for solution: $solutionName..." -ForegroundColor Cyan
    $solutionUrl = "${environmentUrl}api/data/v9.0/solutions?`$filter=uniquename eq '$solutionName'&`$select=solutionid"
    $solutionResponse = Invoke-RestMethod -Uri $solutionUrl -Headers $headers -Method Get
    $solutionId = $solutionResponse.value[0].solutionid

    if (!$solutionId) {
        Write-Host "Solution '$solutionName' not found!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Found solution ID: $solutionId" -ForegroundColor Green

    # Step 2: Get solution components first to find AI Builder models
    Write-Host "Getting solution components for solution ID: $solutionId..." -ForegroundColor Cyan

    # Query solution components to find AI Builder models
    $componentsUrl = "${environmentUrl}api/data/v9.0/solutioncomponents?`$filter=_solutionid_value eq '$solutionId' and componenttype eq 401&`$select=objectid,componenttype"
    Write-Host "Querying solution components: $componentsUrl" -ForegroundColor Gray

    try {
        $componentsResponse = Invoke-RestMethod -Uri $componentsUrl -Headers $headers -Method Get
        
        if ($componentsResponse.value.Count -eq 0) {
            Write-Host "No AI Builder model components found in this solution." -ForegroundColor Yellow
        } else {
            Write-Host "Found $($componentsResponse.value.Count) potential AI components. Checking for AI Builder models..." -ForegroundColor Green
            
            $aiModelIds = @()
            
            # Find all AI Builder model IDs from solution components
            foreach ($component in $componentsResponse.value) {
                $objectId = $component.objectid
                if ($objectId) {
                    $aiModelIds += $objectId
                }
            }
            
            if ($aiModelIds.Count -eq 0) {
                Write-Host "No AI Builder model IDs found in solution components." -ForegroundColor Yellow
            } else {
                Write-Host "Found $($aiModelIds.Count) AI Builder model IDs in solution components." -ForegroundColor Green
                
                # For each AI model ID, query the AI model details
                foreach ($modelId in $aiModelIds) {
                    # Query AI Builder model details
                    Write-Host "`nGetting details for AI model ID: $modelId" -ForegroundColor Cyan
                    
                    $detailsUrl = "${environmentUrl}api/data/v9.0/msdyn_aimodels($modelId)?`$select=msdyn_aimodelid,msdyn_name"

                    Write-Host "Querying model details: $detailsUrl" -ForegroundColor Gray
                    
                    try {
                        $modelDetails = Invoke-RestMethod -Uri $detailsUrl -Headers $headers -Method Get
                        Write-Host "Found AI Builder model: $($modelDetails.msdyn_name)" -ForegroundColor Green
                        $modelDetails | ConvertTo-Json -Depth 10
                    } catch {
                        Write-Host "Error retrieving details for model ID $modelId. This might not be an AI Builder model." -ForegroundColor Yellow
                    }
                }
            }
        }
    }
    catch {
        Write-Host "Error querying solution components: $_" -ForegroundColor Red
        Write-Host $_.Exception.Message
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $responseBody = $reader.ReadToEnd()
            Write-Host $responseBody -ForegroundColor Red
        }
    }

    Write-Host "`nAfter identifying models, you can use the AIBuilderPrompt template to generate automated tests." -ForegroundColor Green
    Write-Host "For example: 'I'd like to generate automated tests for the [ModelName] model.'" -ForegroundColor Green
    ```

## Next Action

After executing this script and identifying the available AI Builder models, you can proceed to generate automated tests for a specific model:

1. Review the list of AI Builder models returned by the script above
2. Choose a model you want to create automated tests for
3. Use the AIBuilderPrompt template to generate comprehensive tests for the selected model

Would you like to generate automated tests for any of the AI Builder models listed above? If so, please specify:
1. The name of the AI Builder model you want to test
2. Any specific test scenarios you want to focus on (optional)
3. Any specific requirements for the generated tests (optional)

Example response:
```
Yes, I'd like to generate automated tests for the "Customer Sentiment Analysis" model. Please focus on:
1. Testing with various text inputs
2. Validating sentiment score thresholds
3. Ensuring proper error handling for malformed inputs
```

Then use the AIBuilderPrompt template to create comprehensive test scenarios and code for your selected AI Builder model.
