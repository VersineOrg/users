#############################################################
#                         Users                             #
#############################################################

Autors: Mickael, Rayan

Last edited: the 27th of april 2022

## The role:

This is the users Mirco service,
It's goal is to recieve request asking to edit the user database, 
check if the asker is authaurized to edit this user through his token and
determine the action asked to do by parsing the endpoint and reading the body of the request,
then the DB is edited and information is returned in the response body for frontend usage.

## The reqests:

This microservice has theese following endpoints: 

-/user/name_of_the_user

-/profile

-/deleteUser

-/editBio

-/editUsername

-/requestFriend

-/deleteRequest


## Features to implement in the future:

delete circles of deleted users, and assign their posts to user_deleted
