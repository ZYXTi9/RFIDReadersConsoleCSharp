SELECT * FROM antenna_info_tbl;
SELECT * FROM antenna_tbl;
SELECT * FROM gpi_tbl;
SELECT * FROM gpo_tbl;
SELECT * FROM power_radio_tbl;
SELECT * FROM read_tbl;
SELECT * FROM reader_settings_tbl;
SELECT * FROM reader_tbl;
SELECT * FROM rf_modes_tbl;
SELECT * FROM singulation_tbl;
SELECT * FROM tag_storage_tbl; 

select * from reader_type_tbl;
call TruncateAllTables();
call TruncateReadTable();

SELECT * FROM antenna_tbl a INNER JOIN singulation_tbl b ON a.AntennaID = 1 WHERE a.ReaderID = 1 AND a.Antenna = 1;

UPDATE reader_tbl SET Status = 'Disconnected' WHERE Status = 'Connected';

UPDATE reader_tbl SET Status = 'Connected' WHERE ReaderID =1 AND ReaderTypeID = 3 AND IPAddress = '192.168.1.241' AND DeviceName = 'csl';

