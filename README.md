# azure-appservice-redis
 How Azure Cache for Redis scales Azure App Services

 ## Start local Redis on Docker Desktop
 `docker compose up -d`

 ## Check Redis container
 ```
 # redis-cli            
127.0.0.1:6379> ping
PONG
127.0.0.1:6379>
 ```

## Open net4-api in Visual Studio Community 2022
- Hit F5 to run locally
- Deploy to your Azure App Service
- Create Azure Cache for Redis, grab its access key to be the connection string, configure app service configuration with the connection string

## Download JMeter
- https://jmeter.apache.org/download_jmeter.cgi
- Update those *.jmx test plans with your app service url
- Run the test plans using the Run*.bat files. File names such as S1_C1_10* are scaling names (App Service - Standard 1, Redis Cache - Basic 1 and 10 instances of the app service)


