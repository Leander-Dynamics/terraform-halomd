 /**                            
 Index name			: ImportFieldConfigs-EOB Date Add Script                          
 Description        : Adding EOB Date in EHRDetails configuration                     
 Purpose		    : 
 DB Server			: de-dwsql13-sql.database.windows.net
 Database Name		: dE-HaloArbit-db                         
 Created by         : Paranthaman.D                         
 Created on         : 09/19/2024                            
 Reviewed by        :                        
 Reviewed on        :                        
**/    

IF EXISTS (SELECT TOP 1 1 FROM [ImportFieldConfigs]  
            WHERE [Source] = 'EHRDetail' 
			     AND [SourceFieldname] ='EOBDate'
				 AND [TargetFieldname] ='EOBDate')
   PRINT ('ImportFieldConfigs-EOB Date already available')  
ELSE 
INSERT INTO [ImportFieldConfigs]
           ([Action]
           ,[IsActive]
           ,[Source]
           ,[SourceFieldname]
           ,[TargetFieldname]
           ,[UpdatedBy]
           ,[UpdatedOn]
           ,[CanBeEmpty]
           ,[IsRequired]
           ,[IsBoolean]
           ,[IsDate]
           ,[IsNumeric]
           ,[IsTracking]
           ,[Description]
           ,[TargetAuthorityKey])
     VALUES
           ('OnlyWhenEmpty'
           ,0
           ,'EHRDetail'
           ,'EOBDate'
           ,'EOBDate'
           ,'paranthaman.dhamodharan@halomd.com'
           ,'2024-08-21 18:39:40.3842413'
           ,1
           ,0
           ,0
           ,1
           ,0
           ,0
           ,'Adding EOBDate column config'
           ,'')


