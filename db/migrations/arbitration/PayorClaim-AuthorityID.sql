select * from [dbo].[ArbitrationCases]
where id = 23913

select top 10 CONVERT(DATE, JSON_VALUE([NSATracking], '$.DateNegotiationSent')) AS DateNegotiationSent from [dbo].[ArbitrationCases]
where JSON_VALUE([NSATracking], '$.DateNegotiationSent') is not null

---
select  * from [dbo].[ArbitrationCases] as a where id = 23914

select c.ArbitrationCaseId, count(*) as total 
	from [dbo].[CaseSettlements] as c 
	group by c.ArbitrationCaseId 
	having count(*) > 2 
	order by total desc

select * from [dbo].[CaseSettlements] c

select c.id, a.Payor, a.Customer, c.AuthorityCaseId from [dbo].[ArbitrationCases] a, [dbo].[CaseSettlements] c
where a.Id = c.ArbitrationCaseId
and c.ArbitrationCaseId = 130404

select * from [dbo].[CaseSettlementCPT] as cpt
select * from [dbo].[CaseSettlementDetails] where [CaseSettlementId] in (30204,32256,32840,33403)

select p.*, c.* from ArbitrationCases a, payors p, Customers c where a.id in (23913,23914,23915,23916)
and a.Payor = p.Name and a.Customer = c.Name


SELECT [a].[Id], [a].[Payor], [a].[Customer]
FROM [ArbitrationCases] AS [a]
WHERE [a].[Id] IN (23913, 23914, 23915, 23916)

Select AuthorityCaseId, * from [dbo].[ArbitrationCases] where payorclaimnumber = '22340000000'
----
select * from [dbo].[CaseSettlementDetails] c, ArbitrationCases a 
where c.ArbitrationCaseId = a.id
and c.AuthorityCaseId = '546645'
