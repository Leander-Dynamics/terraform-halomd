select * from [dbo].[ArbitrationCases] where [PatientName] = 'Gina Bynoe-Dammann'

update [dbo].[ArbitrationCases]
set id = 23913
where id = 216907

select NSATracking from ArbitrationCases where id in (216995,216994,216993,216992)

DECLARE @json NVARCHAR(MAX);
SET @json = (
   select NSATracking from ArbitrationCases where id = 216994   -- Specify your condition to identify the row
);

SET @json = JSON_MODIFY(
    @json,
    '$.DateNegotiationSent',
    null -- New value for DateNegotiationSent
);

UPDATE ArbitrationCases
SET NSATracking = @json
WHERE id = 216994   ; -- Specify the same condition to identify the row

===state===

select [AuthorityStatus] from ArbitrationCases where id = 107490 --nsa

select distinct authority from ArbitrationCases 
select * from [dbo].[Authorities]

