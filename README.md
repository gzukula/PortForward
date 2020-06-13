# Why?

### Client/Server:
Why one needs to write a TCP client/server when there are already tools like 'netcat'? At a place where I work, the corporote antivirus was deleting the netcat since it was identified as a "hacker" tool. I didn't bother looking for another, and thought it would be quicker to write one myself. This kept my antivirus scanner happy.

### Port-forwarding:
An obvious choice for Windows is to use a command like this:
netsh interface portproxy add v4tov4 listenaddress=127.0.0.1 listenport=9000 connectaddress=192.168.0.10 connectport=80
Nevertheless I ended writing port-forwarding as I was investigating some networking issues, and wanted to learn the specific error why traffic didn't go through. Also every year or two I find myself writing such tool anyway, whether it's for forking some data or some other reason - so I need to have an actual code running and to have a better control on how this port-forwarding is performed. Also it's .NET Core, so theoretically this code is cross-plaform :)