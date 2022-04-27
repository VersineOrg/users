#############################################################
#                         Users                             #
#############################################################

Autor: Mickael

Last edited: the 27th of april 2022

The role:

This is the users Mirco service,
It's goal is to recieve request asking to edit the user database, 
check if the asker is authaurized to edit this user through his token and
determine the action asked to do by parsing the endpoint and reading the body of the request,
then the DB is edited and information is returned in the response body for frontend usage.

The reqests:

This micro service have many endpoints: 

-/user/edit

-/user/delete

-/user/addfriend (not implemented)

-/user/removefriend (not implemented)

This is an example of a request to this micro service:

http://hostname:port/user/edit

Method:POST

BODY:
{
"token":"ewogICJhbGdvIjogIkhTMjU2IiwKICAidHlwZSI6ICJKV1QiCn0=.ewogICJpZCI6ICI2MjY4MTc5M2Q0NjMyNGMzOTE5YjIzMTEiLAogICJleHAiOiAiMCIKfQ==.Pz4kags/CHIFPyxLPxF2Pz8pSj8vP14/VVU/P3U0JT8="

"changed":{

    "bio":"this is the new bio",

    "username":"this is the new username"

    "otherfield":"newvalue"
}

The response:

This is an example of response:

{

"status": "success",

"message": "user edited",

"data": "{ \"username\" : \"pkngr\", \"password\" : \"1c333953d794cb13190d0e51db6350795e218921feed13d2840f0015567450d3\", \"ticket\" : \"pkngr-presentletter\", \"ticketCount\" : 10, \"avatar\" : \"https://i.imgur.com/k7eDNwW.jpg\", \"bio\" : \"je host le serveur\", \"banner\" : \"https://images7.alphacoders.com/421/thumb-1920-421957.jpg\", \"color\" : \"28DBB7\", \"friends\" : [], \"circles\" : [], \"incomingFriendRequests\" : [], \"outgoingFriendRequests\" : [] }"

}

Features to implement in the future:

Ask the password to edit username to define a new hashed password with the new username as salt.

Add the same rules for username as for username changes

check with rayan if the addfriend / remove friend should be in users or circle MS.

 
