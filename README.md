# azure-function-notification

A solution to automate the sending of notifications on a set schedule. The solution makes use of the following technologies:

* Azure Serverless Functions - Employs a timer function to schedule and orchestrate the processing of the triggers.
* Google Sheets - Holds the database of triggers and messages in an easily accessible and updatable format.
* Twilio - Used for outgoing SMS, Phone and Email for notifications.