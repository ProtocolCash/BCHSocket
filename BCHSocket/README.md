# BCHSocket

Bitcoin BCH Websocket!

Takes raw blocks and transactions from your node. Accepts websocket connections and allows clients to
subscribe to broadcasts for Blocks, OpReturns, and Output Addresses.

# Getting Started

BCHSocket requires minimal configuration to get started with your own Websocket server. Presuming you already have a node, there isn't even any sync time required! Edit bitcoin.conf to include zmqpubrawtx and zmqpubrawblock, setup BCHSocket's app.config to match, then just start BCHSocket!

## Development Environment and Tools
BCHSocket and SharpBCH can be built under any DotNET Core 2.2 environment.

Recommended Development Environment:
- Visual Studio 2017 Community Edition https://visualstudio.microsoft.com/downloads/
- JetBrains Resharper https://www.jetbrains.com/resharper/
- DotNET Core 2.2 https://dotnet.microsoft.com/download/dotnet-core/2.2

## Dependencies

BCHSocket and it's main dependency SharpBCH (https://github.com/ProtocolCash/SharpBCH) aim to keep dependency trees minimal.

BCHSocket depends on:
- NETCore 2.2.0
- System.Configuration.ConfigurationManager
- SharpBCH

## Building
Build Release from CLI:
- *dotnet publish -c release -r {target_system}*

Where *{target_system}* is e.g. win10-x64 or linux-x64 or osx.10.14-x64. 
(see RID catalog for all runtime IDs: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)

## Configuration
See *app.config* (or, in release version, *BCHSocket.exe.config*). The following options are available:

|Option Name|Description|Example|
|--|--|--|
|ZMQTxHostname|Hostname for ZMQ Raw Transaction Publisher|e.g. 127.0.0.1 or specific IP|
|ZMQTxPort|Port number for ZMQ Raw Transaction Publisher|e.g. 28333|
|ZMQBlockHostname|Hostname for ZMQ Raw Block Publisher|e.g. 127.0.0.1 or specific IP|
|ZMQBlockPort|Hostname for ZMQ Raw Block Publisher|e.g. 28331|
|WebsocketBindIP|Local IP to which to bind Websocket Server|e.g. 127.0.0.1 or 0.0.0.0 for all interfaces|
|WebsocketBindPort|Local Port to which to bind Websocket Server|e.g. 8181|

ZMQ options must match local ZMQ publisher configuration.

## Websocket Client Commands
The following commands are valid:

|Command|Details|
|--|--|
|{"op": "block"}|Subscribe to block updates|
|{"op": "transactions"}|Subscribe to transaction updates|
|{"op": "address", "address": "[CASH_ADDRESS]"}|Subscribe to transaction updates that include outputs to CASH_ADDRESS|
|{"op": "opreturn", "prefix": "[PREFIX(HEX)]"}|Subscribe to transaction updates that include an OP_RETURN output with matching prefix (represented as hex).

Any command above can also be sent with "rm_" prefixed to the op string to remove an existing subscription. e.g. {"op": "rm_block"} will remove an existing block subscription.

## Websocket Responses and Broadcasts
### Errors
Websocket will respond with JSON object where "op" param is "error",  and "error" param is the error message. An error in response to a subscription request means the subscription request failed.

Examples: 
- { "op": "error", "error": "Error while parsing JSON request. Expected 'op' parameter as string." }
- { "op": "error", "error": "Error while parsing JSON request. Expected 'prefix' parameter as string." }
- { "op": "error", "error": "Error while parsing JSON request. Expected 'prefix' parameter to be less than 32 hex characters long." }

### Subscriptions Requests
Websocket will confirm all subscription requests with a JSON response indicating success or failure.

Generically, this will look like: { \"op\": \"[SUBSCRIPTION_TYPE]\", \"result\": \"ok\" }, { \"op\": \"rm_[SUBSCRIPTION_TYPE]\", \"result\": \"ok\" }, or { \"op\": \"rm_[SUBSCRIPTION_TYPE]\", \"result\": \"failed\" }

Examples:
- { \"op\": \"address\", \"result\": \"ok\" }
- { \"op\": \"opreturn\", \"result\": \"ok\" }
- { \"op\": \"rm_address\", \"result\": \"ok\" }
- { \"op\": \"rm_address\", \"result\": \"failed\" }

### Broadcasts
Subscribed Websocket clients will receive broadcasts for all subscribed events. 

A client subscribed to block updates would receive these broadcasts when the new blocks were seen:
- { "op": "new_block", "blockHash": "00000000000000000301C020CA2F01B9B8C5B665F649DDC417EE7BE59B8F99E8", "prevBlockHash": "000000000000000000E6D9A0A73B6C803A1A1200FEF7DB64445B7366FBEFB781", "transactions": 283 }
- { "op": "new_block", "blockHash": "000000000000000000A2A32BA22DA5732A5C97AFCBE5BC28F5CCB20121F2342F", "prevBlockHash": "00000000000000000301C020CA2F01B9B8C5B665F649DDC417EE7BE59B8F99E8", "transactions": 416 }

A client subscribed to transaction updates (based on matching output address or opreturn) will receive updates as follows:
- { "op": "new_tx", "txid": "[TXID(HEX)]", "inputs": [NUMBER_OF_INPUTS], "outputs":  { [OUTPUTS] } }

Where [OUTPUTS] is an array of outputs, where each output is formatted:
- { "type": "[OUTPUT_TYPE]", "address": "[ADDRESS]", "value": "[VALUE_IN_SATOSHI]", "script": "[RAW_SCRIPT(HEX)]" }

Where:
- [OUTPUT_TYPE] is one of: P2PKH, P2SH, DATA, or OTHER
- [ADDRESS] is a Cash Address, or blank string if output type is not P2SH or P2PKH
- [VALUE_IN_SATOSHI] is the amount spent to that output
- [RAW_SCRIPT(HEX)] is the raw bitcoin script for this output, represented in HEX.
