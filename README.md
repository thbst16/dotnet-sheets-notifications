# azure-function-notification

[![Build Status](https://beckshome.visualstudio.com/azure-function-notification/_apis/build/status/thbst16.azure-function-notification?branchName=main)](https://beckshome.visualstudio.com/azure-function-notification/_build/latest?definitionId=8&branchName=main)

A solution to automate the sending of notifications on a set schedule. The solution makes use of the following technologies:

* Azure Serverless Functions - Employs a timer function to schedule and orchestrate the processing of the triggers.
* Google Sheets - Holds the database of triggers and messages in an easily accessible and updatable format.
* Twilio - Used for outgoing SMS, Phone and Email for notifications.