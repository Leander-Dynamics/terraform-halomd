
 /**                            
 Script name		: 2508 - PrevailingParty & AwardAmount data's transfer from DisputeAwardDeterminations to DisputeMaster                         
 Description        : Transferring prevailingParty & AwardAmount data's transfer from DisputeAwardDeterminations to DisputeMaster                  
 DB Server			: DE-DWSQL21-VM
 Database Name		: SRC_IDRSupport                         
 Created by         : Paranthaman.D                         
 Created on         : 09/13/2024                            
 Reviewed by        :                        
 Reviewed on        :    
**/ 

/** Query execution procedure
     0. For safer side make the backup of DisputeMaster, DisputeAwardDeterminations tables once after the data verificaton done 
     1. Verify the Before update , During update and after update data's for anyone of the available dispute number
	 2. Once it is verified remove the where clause condition 
	 3. Remove "ROLLBACK TRANSACTION;" and uncomment "COMMIT TRANSACTION;"
	 4. Execute the query again
	 5. Now verify the after update data's
	 6. If any issue noticed restore the backup 

**/

-- Backup DisputeMaster, DisputeAwardDeterminations tables once after data verification process done

	--SELECT * INTO BackupDisputeMaster_2508 FROM DisputeMaster;

	--SELECT * INTO DisputeAwardDeterminations_2508 FROM DisputeAwardDeterminations;

-- Take the Data's from both tables before doing update
SELECT  dm.AwardAmount , dd.TotalAwardAmount,
	    dm.PrevailingParty ,dd.PrevailingParty 
		FROM DisputeMaster dm 
		INNER JOIN DisputeAwardDeterminations dd ON dm.DisputeNumber = dd.DisputeNumber
		WHERE dm.DisputeNumber ='DISP-1067892'

BEGIN TRANSACTION

-- Update dispute master''s prevailingParty & AwardAmount with DisputeAwardDeterminations TotalAwardAmount & PrevailingParty
UPDATE dm 
	SET dm.AwardAmount = dd.TotalAwardAmount,
	    dm.PrevailingParty = dd.PrevailingParty 
	FROM DisputeMaster dm 
		INNER JOIN DisputeAwardDeterminations dd ON dm.DisputeNumber = dd.DisputeNumber
		WHERE dm.DisputeNumber ='DISP-1067892'

-- Verify the DisputeMaster data after update
SELECT  dm.AwardAmount , dd.TotalAwardAmount,
	    dm.PrevailingParty ,dd.PrevailingParty 
		FROM DisputeMaster dm 
		INNER JOIN DisputeAwardDeterminations dd ON dm.DisputeNumber = dd.DisputeNumber
		WHERE dm.DisputeNumber ='DISP-1067892'

-- By default its rollback,  Commit the transaction once the verification status is OK, 

   -- COMMIT TRANSACTION;

ROLLBACK TRANSACTION;

-- Verify the DisputeMaster data with rollback and with commit
SELECT  dm.AwardAmount , dd.TotalAwardAmount,
	    dm.PrevailingParty ,dd.PrevailingParty 
		FROM DisputeMaster dm 
		INNER JOIN DisputeAwardDeterminations dd ON dm.DisputeNumber = dd.DisputeNumber
		WHERE dm.DisputeNumber ='DISP-1067892'

/**
  * This below restore process is required only if any issues found after data transfer else not required
  Restoring from BackupDisputeMaster_2508 is not possible by default insert into command 
  since the ID column is an Auto identity , So rename the BackupDisputeMaster_2508 As DisputeMaster
**/

 -- SP_RENAME 'DisputeMaster', 'DisputeMaster_old'

 -- SP_RENAME 'BackupDisputeMaster_2508', 'DisputeMaster'
