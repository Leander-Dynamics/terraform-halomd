USE SRC_IDRSupport


 /**                          
 Script name		: 2508 - Remove PrevailingParty & TotalAwardAmount from DisputeAwardDeterminations                         
 Description        : Removing prevailingParty & TotalAwardAmount columns from DisputeAwardDeterminations                  
 DB Server			: DE-DWSQL21-VM
 Database Name		: SRC_IDRSupport                         
 Created by         : Paranthaman.D                         
 Created on         : 09/11/2024                            
 Reviewed by        :                        
 Reviewed on        :                        
**/  


-- 1. Remove TotalAwardAmount
IF  EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DisputeAwardDeterminations' 
    AND COLUMN_NAME = 'TotalAwardAmount'
)

BEGIN

Declare @TotalAwardAmount_Default_constraint varchar(MAX);

SET @TotalAwardAmount_Default_constraint = (SELECT name FROM sys.default_constraints 
									WHERE parent_object_id = OBJECT_ID('DisputeAwardDeterminations')			
									AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('DisputeAwardDeterminations'),
									'TotalAwardAmount', 'ColumnId'));

-- Use dynamic SQL to drop the constraint
DECLARE @sql NVARCHAR(MAX);
SET @sql = 'ALTER TABLE DisputeAwardDeterminations DROP CONSTRAINT ' + @TotalAwardAmount_Default_constraint;

-- Execute the dynamic SQL
EXEC sp_executesql @sql;

-- Drop the column from 
ALTER TABLE PrinterMaster
DROP COLUMN TotalAwardAmount;

END



--2. Remove PrevailingParty
IF  EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DisputeAwardDeterminations' 
    AND COLUMN_NAME = 'PrevailingParty'
)

BEGIN

Declare @PrevailingParty_Default_constraint varchar(MAX);

SET @PrevailingParty_Default_constraint = (SELECT name FROM sys.default_constraints 
									WHERE parent_object_id = OBJECT_ID('DisputeAwardDeterminations')			
									AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('DisputeAwardDeterminations'),
									'PrevailingParty', 'ColumnId'));

-- Use dynamic SQL to drop the constraints
DECLARE @sql1 NVARCHAR(MAX);
SET @sql1 = 'ALTER TABLE DisputeAwardDeterminations DROP CONSTRAINT ' + @PrevailingParty_Default_constraint;

-- Execute the dynamic SQL
EXEC sp_executesql @sql1;

-- Drop the column from 
ALTER TABLE DisputeAwardDeterminations
DROP COLUMN PrevailingParty;

END

GO
