1.	Goto HOMEOS2\homeos2\Hub\Common\TrustedServer\bin\Release>
2.	As an administrator start metadata server on 23456: MetaDataServer.exe http://localhost:23456/MetaDataServer
3.	Inside visual studio: Update DataStore’s service reference to MetaDataService to point to this. Use configure service reference.
    - http://stackoverflow.com/questions/3977560/service-reference-error-failed-to-generate-code-for-the-service-reference
4.	Update endpoint address in your applications app.config . See Tools/HDS/HDS_Eval for an example.
