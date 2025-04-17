@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

param principalType string

param principalId string

resource asb 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: take('asb-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'asb'
  }
}

resource asb_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(asb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalType: principalType
  }
  scope: asb
}

resource weatherapp_modeling_data_accepted 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'weatherapp-modeling-data-accepted'
  parent: asb
}

resource weatherapp_modeling_data_rejected 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'weatherapp-modeling-data-rejected'
  parent: asb
}

resource weatherapp_model_updated 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'weatherapp-model-updated'
  parent: asb
}

resource weatherapp_user_notification 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'weatherapp-user-notification'
  parent: asb
}

output serviceBusEndpoint string = asb.properties.serviceBusEndpoint
