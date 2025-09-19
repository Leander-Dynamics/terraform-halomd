USE SRC_IDRSupport

 /**                            
 Description        : Adding BriefAssignedDate column to DisputeMaster tables
 Tablea affected    : 1. DisputeMaster
 Purpose		    : To populate 'Brief Assigned Date' in the Brief Details Section on the Dispute Page
 DB Server			: DE-DWSQL21-VM
 Database Name		: SRC_IDRSupport                         
 Created by         : Paranthaman.D                         
 Created on         : 09/23/2024                            
 Reviewed by        :                        
 Reviewed on        :                        
**/   

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DisputeMaster' 
    AND COLUMN_NAME = 'BriefAssignedDate'
)
ALTER TABLE DisputeMaster
ADD BriefAssignedDate  DATE NULL;

GO
