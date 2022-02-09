param name string = uniqueString(resourceGroup().id)

param locations array = []

var location = resourceGroup().location

var allLocations = contains(locations, location) ? locations : concat([
  location
], intersection([
  location
], locations))

#disable-next-line BCP081
resource registry 'Microsoft.ContainerRegistry/registries@2021-12-01-preview' = {
  name: name
  location: location
  sku: {
    name: 'Premium'
  }
  properties: {
    adminUserEnabled: true
    anonymousPullEnabled: true
  }
}

#disable-next-line BCP081
resource replications 'Microsoft.ContainerRegistry/registries/replications@2021-12-01-preview' = [for (loc, i) in allLocations: if (toLower(location) != toLower(loc)) {
  name: '${name}/${loc}'
  location: loc
  dependsOn: [
    registry
  ]
}]

output url string = registry.properties.loginServer

#disable-next-line outputs-should-not-contain-secrets
output username string = registry.listCredentials().username

#disable-next-line outputs-should-not-contain-secrets
output password string = registry.listCredentials().passwords[0].value
