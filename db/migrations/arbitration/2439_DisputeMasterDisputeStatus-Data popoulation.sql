
 /**                            
 Script name		: DisputeMasterDisputeStatus-Data popoulation                          
   GO
 Purpose		    : To populate new status in Dispute-UI 
 DB Server			: DE-DWSQL21-VM
 Database Name		: SRC_IDRSupport                         
 Created by         : Paranthaman.D                         
 Created on         : 28/08/2024                            
 Reviewed by        :                        
 Reviewed on        :                        
**/  
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[REF_DisputeMasterDisputeStatus]  WHERE DisputeStatus = 'Awarded – Additional Information Requested')
   INSERT INTO [dbo].[REF_DisputeMasterDisputeStatus] (DisputeStatus) VALUES('Awarded – Additional Information Requested')

   GO