select  disputeNumber, arbitid, count(*) as total from [dbo].[DisputeCPT]
where ArbitID = 23913
group by disputeNumber, arbitid
having count(*) > 1
order by total desc

--update [dbo].[DisputeCPT]
--set arbitid = 216907
--where  ArbitID = 23913

-- one arbit can have many disputes
select disputeNumber, arbitid from [dbo].[DisputeCPT] --, count (*) as total  -- where disputeNumber in ('DISP-1247584','DISP-1114989','DISP-1114873','DISP-1198468','DISP-1186655','DISP-1180511','DISP-1180865','DISP-1169657','DISP-1316643','DISP-1139685','DISP-1141723','DISP-1139506','DISP-1149594','DISP-1168168','DISP-1174912','DISP-1172856','DISP-1233167','DISP-1247253','DISP-1130077','DISP-1114925','DISP-1247293','DISP-1247446','DISP-1168701','DISP-1396057','DISP-1138627','DISP-1141029','DISP-1247356','DISP-1247404','DISP-1233088','DISP-1125713','DISP-1137233','DISP-1130388','DISP-1122707','DISP-1126491','DISP-1216274','DISP-1232368','DISP-1212268','DISP-1212611','DISP-1247390','DISP-1247620','DISP-1247672','DISP-1172605')
--group by disputeNumber, arbitid
--having count (*) > 1
order by disputeNumber, arbitid desc

select * from [dbo].[DisputeCPT] where disputeNumber not like '%DISP-%' 

select distinct disputeNumber, count (*) from [dbo].[DisputeCPT] group by disputeNumber having count (*) > 1

--One dispute can have multiple arbitid 
select disputeNumber, arbitid  from [dbo].[DisputeCPT] where disputeNumber = 'DISP-101465' group by DisputeNumber, arbitid

--Conclusion many - many relationship exists between dispute and arbit 

select distinct arbitid, DisputeNumber from [dbo].[DisputeCPT] where disputeNumber = 'DISP-101465'
order by arbitid asc

--I will get dispute number
s
--get arbit id
update [dbo].[DisputeCPT]
set arbitid = 216995
where arbitid = 23913
--select * from [dbo].[DisputeCPT] 
select distinct disputeNumber from [dbo].[DisputeCPT] order by 1 -- where disputeNumber = 'DISP-1115307' 

