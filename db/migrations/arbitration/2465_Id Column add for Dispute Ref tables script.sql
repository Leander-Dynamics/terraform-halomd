USE SRC_IDRSupport

 /**                            
 Description        : Adding Id columns to dispute ref tables
 Tablea affected    : 1. REF_DisputeMasterCertifiedEntity, 
					  2. REF_DisputeMasterCustomer
					  3. REF_DisputeMasterDisputeStatus
					  4. REF_DisputeMasterEntity
					  5. REF_DisputeMasterServiceLine
					  6. REF_StatusPriority

 Purpose		    : To speed up the retrieval of rows from the table
 DB Server			: DE-DWSQL21-VM
 Database Name		: SRC_IDRSupport                         
 Created by         : Paranthaman.D                         
 Created on         : 09/06/2024                            
 Reviewed by        :                        
 Reviewed on        :                        
**/   

-- 1
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'REF_DisputeMasterCertifiedEntity' 
    AND COLUMN_NAME = 'Id'
)
ALTER TABLE [REF_DisputeMasterCertifiedEntity]
ADD  [Id] [int] IDENTITY(1,1) PRIMARY KEY

GO

-- 2
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'REF_DisputeMasterCustomer' 
    AND COLUMN_NAME = 'Id'
)
ALTER TABLE [REF_DisputeMasterCustomer]
ADD  [Id] [int] IDENTITY(1,1) PRIMARY KEY

GO

-- 3
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'REF_DisputeMasterDisputeStatus' 
    AND COLUMN_NAME = 'Id'
)
ALTER TABLE [REF_DisputeMasterDisputeStatus]
ADD  [Id] [int] IDENTITY(1,1) PRIMARY KEY

GO

-- 4
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'REF_DisputeMasterEntity' 
    AND COLUMN_NAME = 'Id'
)
ALTER TABLE [REF_DisputeMasterEntity]
ADD  [Id] [int] IDENTITY(1,1) PRIMARY KEY

GO

-- 5
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'REF_DisputeMasterServiceLine' 
    AND COLUMN_NAME = 'Id'
)
ALTER TABLE [REF_DisputeMasterServiceLine]
ADD  [Id] [int] IDENTITY(1,1) PRIMARY KEY

GO

-- 6
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'REF_StatusPriority' 
    AND COLUMN_NAME = 'Id'
)
ALTER TABLE [REF_StatusPriority]
ADD  [Id] [int] IDENTITY(1,1) PRIMARY KEY

GO
