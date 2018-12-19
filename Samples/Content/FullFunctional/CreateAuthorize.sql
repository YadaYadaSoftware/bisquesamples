if exists(select * from sys.databases where name = 'Authorize')
begin
	print 'exists'
end
else
begin
	print 'does not exist'
	create database Authorize
end
	