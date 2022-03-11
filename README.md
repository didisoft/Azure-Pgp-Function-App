# DidiSoft Azure Functons running PGP encryption

This repo contains four projects that demonstrate how to spawn an Azure Batch service from a BrobTrigger that listens for new blobs in an Azure Blob Storage container. The Batch Service encrypts the input Blob into another container. A separate Blob Function listens for the encrypted Blobs and then stops the Batch Service.

Projecfts in this Solition (You will need Visua Studio 2019 or Raider):

  - BatchService - illusrtative Batch Service 
  - EncryptBlobPgp - standalone EXE that performs the encryption
  - DidiSoftBlobFunction - starts a Batch Service that invokes EncryptBlobPgp
  - DidiSoftBlobDeleteFunction - stops the Batch Service

# Publisng in Azure!

  - create a Batch Service and its credentials must be set in all projects
  - register EncryptBlobPgp as Application in the Batch Service
  - change the names of the Input and Output containers in DidiSoftBlobFunction and DidiSoftBlobFunction
  - publish DidiSoftBlobFunction and DidiSoftBlobFunction
  
You may also need the [DidiSoft.Pgp] assembly.

License
----
MIT

   [DidiSoft.Pgp]: <https://www.nuget.org/packages/DidiSoft.Pgp.Trial/>
   [df1]: <https://didisoft.com/>
   [AngularJS]: <http://angularjs.org>
   [Gulp]: <http://gulpjs.com>

