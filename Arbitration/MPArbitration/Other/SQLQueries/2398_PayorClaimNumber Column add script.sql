USE SRC_IDRSupport

 /**                            
 Description        : Adding PayorClaimNumber columns to DisputeCPT tables
 Tablea affected    : 1. DisputeCPT
 Purpose		    : To populate 'Payor Claim Number' on CPT Level
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
    WHERE TABLE_NAME = 'DisputeCPT' 
    AND COLUMN_NAME = 'PayorClaimNumber'
)

ALTER TABLE DisputeCPT
ADD PayorClaimNumber varchar(200)  DEFAULT NULL;

GO
