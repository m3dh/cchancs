CREATE USER 'cchan_svc'@'%' IDENTIFIED BY 'T%nt0wn';
GRANT SELECT,INSERT,UPDATE,DELETE,CREATE ON cchan_core.* TO 'cchan_svc'@'%';
GRANT SELECT,INSERT,UPDATE,DELETE,CREATE ON cchan_data.* TO 'cchan_svc'@'%';