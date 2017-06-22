# Instancely

Instancely is an alternative AWS EC2 and RDS dashboard that sorts instances by their respective `Team`, `Environment`, and `Application`. The original concept for Instancely was developed by Michael Lorant (https://github.com/mikelorant).

### Requirements
1. AWS access keys with full access to EC2 and RDS
2. OpenID access keys and the authority URL
3. Slack incoming webhook URL
2. EC2 and RDS instances with the following key-value pairs:

   Team = team_name

   Environment = environment_name

   Application = application_name
   
   Name = instance_name

### Usage (.NET)
1. Run `git clone git@github.com:cvandal/instancely.git` or `git clone https://github.com/cvandal/instancely.git`
2. Run `dotnet restore`, followed by `dotnet build`, followed by `dotnet publish`
3. Run the following commands to add the required environment variables:
```
$env:ASPNETCORE_URLS = "http://*:5000"
$env:AWS_ACCESS_KEY = "AKIxxx"
$env:AWS_SECRET_KEY = "abc123"
$env:OPENID_CLIENTID = "instancely"
$env:OPENID_CLIENTSECRET = "abc123"
$env:OPENID_AUTHORITY = "https://auth.yourdomain.com/"
$env:SLACK_WEBHOOK = "https://hooks.slack.com/services/xxx/yyy/zzz"
```
4. Run `dotnet run`

### Usage (Docker)
1. Run `git clone git@github.com:cvandal/instancely.git` or `git clone https://github.com/cvandal/instancely.git`
2. Run `dotnet restore`, followed by `dotnet build`, followed by `dotnet publish`
3. Create a file named `env.list` with the following content:
```
ASPNETCORE_URLS=http://*:5000
AWS_ACCESS_KEY=AKIxxx
AWS_SECRET_KEY=abc123
OPENID_CLIENTID=instancely
OPENID_CLIENTSECRET=abc123
OPENID_AUTHORITY=https://auth.yourdomain.com/
SLACK_WEBHOOK=https://hooks.slack.com/services/xxx/yyy/zzz
```
4. Run `docker build <image_name>:<image_tag> .`
5. Run `docker run -itd --env-file ./path/to/env.list -p 5000:5000 <image_name>:<image_tag>`

### Screenshots
Coming Soon

### Known Issues
`¯\_(ツ)_/¯`
