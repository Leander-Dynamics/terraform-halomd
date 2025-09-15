USE SRC_IDRSupport


 /**                            
 Script name		: 2508 - Add PrevailingParty & AwardAmount in  DisputeMaster                         
 Description        : Adding new columns revailingParty & AwardAmount to DisputeMaster                      
 DB Server			: DE-DWSQL21-VM
 Database Name		: SRC_IDRSupport                         
 Created by         : Paranthaman.D                         
 Created on         : 09/11/2024                            
 Reviewed by        :                        
 Reviewed on        :                        
**/  

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DisputeMaster' 
    AND COLUMN_NAME = 'PrevailingParty'
)

ALTER TABLE DisputeMaster
ADD PrevailingParty varchar(100)  DEFAULT NULL;

GO

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DisputeMaster' 
    AND COLUMN_NAME = 'AwardAmount'
)

ALTER TABLE DisputeMaster
ADD AwardAmount decimal(18,2)  DEFAULT NULL;

GO
