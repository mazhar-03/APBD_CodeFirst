I used Docker. Here's my app settings configuration:

In my configuration file there is a "ConnectionString" which holds the value for the "DefaultConnection"

In that connection I have ->

Server: Specify the server address with port.  For example: ************,&&&&

Database: Specify name of the database.

User Id: Specify the the user ID for authentication.

Password: Specifies the password for the user ID, again for authanticate the connection

TrustServerCertificate: Boolean value, for ignoring SSL certificate validation

*******************************************************************************

Also I added Jwt settings which contains:

Issuer: Responsible for the tokens(sender)

Audience: The reciever of the issuer (the value is same for the issuer)

Key: Random key for safety (used password generator)

ValidInMinutes: Specify the validation time of the token that we generate. In my case I set it to 20 miinutes. By meaning that, after 20 mins we have to regenerate a new token.
