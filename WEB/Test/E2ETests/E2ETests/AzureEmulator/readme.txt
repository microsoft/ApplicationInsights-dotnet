Azureemulator cannot be changed to listen to any ip address other than 127.0.0.1, so its usefull for access from 
outside the container.
To workaround this limitation, Nginx is setup to act as reverse proxy to Azure emulator.
Again, there was issues getting Ngix work inside docker for windows when the downloaded .zip
file was modified to match our required proxy setting. To workaround this, a .zip file containing 
the modified config is checked in to the repo.
This zip can be hosted elsewhere and download on the fly as well.
nginx.conf file in this same folder is the same content as put inside the .zip file as well.