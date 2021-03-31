# ICAP Request Processing

ICAP Request Processing is a process to perform the Glasswall Rebuild functionality on the specified file. It also handles the sending of an outcome message to an RabbitMQ instance.

### Built With
- .NET Core
- Docker

## Submodule
This repo contains a git submodule to the private repo filetrust/sdk-rebuild within the lib folder.

### Updating the submodule 

To update the submodule to a different commit of the filetrust/sdk-rebuild repo, follow these steps:

```
git clone https://github.com/filetrust/icap-request-processing.git
cd .\icap-request-processing\
git checkout -b <BRANCH NAME> origin/develop
git submodule init
git submodule update
cd .\lib\
git checkout <COMMIT HASH>
git commit -am "Updated Glasswall SDK to <ENGINE VERSION>"
git push origin <BRANCH NAME>
```
