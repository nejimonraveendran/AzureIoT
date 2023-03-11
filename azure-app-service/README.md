# How to Create Azure App Service Web App
This document details how to provision an App Service Plan and a Web App in Azure.

Prerequisite:  You need to have a valid Azure subscription.

## Steps
- Log in to Azure portal, and in the search bar, search for "App Services".  From the dropdown menu, choose "App Services".  On the next page that appears, click the "Create" button.
    
    ![image](https://user-images.githubusercontent.com/68135957/223549712-6a311a75-8ad6-4957-976e-b249fb67dedc.png)

- On the "Create Web App" page, under "Basics" tab, fill the necessary details:
  - **Subscription:**  Your subscription.
  - **Resource group:** Create a new resource group or choose an existing one.  If your solution contains multiple resources, it makes sense to put all of them into the same resource group so that you can manage them as a single group (eg. apply policies, delete with one click, etc).
  - **Name:** Give a name for your app without any spaces.  This name is importat because it will be reflected on the URL of your application like "*appname*.azurewebsites.net" (in our example, applianceapp.azurewebsites.net).
  - **Publish:** This is the type of deployment package we plan to use. I would choose "Code" here because I plan to push the build output of my .NET 6 application directly. If you plan to deploy a Docker image, it should be "Docker container."  In that case, you should enable Docker support for your project in Visual Studio.
  - **Runtime:** Your application's runtime stack.  Since I am plannig to build a .NET 6 application, I will choose ".NET 6 (LTS)" here.
  - **Operating System:** The server operating system you want to use. I will choose "Windows".  Since this is a .NET 6 application, "Linux" is also a possibility.
  - **Region:** Your preferred Azure region.  You might want to choose a location closest to the users of your application for the best performance.  In enterprise scenarios, you might also take into consideration the data residency and compliance policies of your organization.
  - **Windows Plan:** This is the name of the App Service Plan we want to use.  An App Service Plan is a tier of Virtual Machine (VM) with a certain compute size.  You can look at various pricing plans available and choose one that is most suitable for your needs (compute, storage, features, SLA, cost, etc.).  For small use cases, S1 plan is good enough.
   

    For reference, here is the screenshot of a completed form.  Click Next.

    ![image](https://user-images.githubusercontent.com/68135957/223555714-c5648e45-9cea-47e7-8a52-500a37fa7edc.png)

 - On the "Deployment" tab, enable or disable GitHub action.  I would keep it disabled because I am not planning to do deployments directly from my GitHub repo.  Click Next.
 - On the "Networking" tab, 
   - **Enable public access:** I would keep this option "on" because I want my application to be accessible through the Internet.  
   - **Enable network injection:** I would keep this one "off", but if your application is part of an enterprise solution and you want to provide communication with an existing virtual network (VNet), you will want to choose "on" and select the appropriate VNet.
- On the "Monitoring" tab, choose "Yes" for the "Enable Application Insights" option. Azure Application Insights is an application performance monitoring tool, which can give valuable insights into how our application is performing.  Read more about Application Insights [here](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview?tabs=net).  Click Next.
- On the "Tags" tab, you can add new tags. Tags can be used to add custom information to your app that can be used for various report generations, etc.  For example, you can tag a resource as "IT-Dept" to mark it as a resource belonging to the IT department.  Later, you can generate usage reports, etc., filtered by those tags.  Read more about tags [here](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/tag-resources?tabs=json).  Tags are optional, but I would add "environment" : "development" as name-value pair here.  
- Finally, click "Review and Create", then "Create". After a while, click the "Go to resource" button that appears. Now, you should see the following resource "Overview" page:

![image](https://user-images.githubusercontent.com/68135957/223563755-a2e236be-a022-4711-8c95-17a79ef1959f.png)


At this point, if you click the "Default Domain" URL (applianceapp.azurewebsites.net), you will be taken to a dummy home page.  Now you are ready to deploy your .NET 6 web application. To deploy the application, you can use different approaches.  Some of them are:
1. For simple scenarios like personal projects, use the Publish feature in Visual Studio with Azure as the target, [as described here](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore?tabs=net60&pivots=development-environment-vs).
2. Use simple Azure CLI scripting as [described here](https://medium.com/@nejimon.raveendran/automate-deployment-of-azure-web-apps-using-azure-cli-and-powershell-adbcaa06e236). 
3. For more enterprise scenarios, use a full-fledged CI/CD system like [Azure Pipelines](https://learn.microsoft.com/en-us/azure/devops/pipelines/get-started/what-is-azure-pipelines?view=azure-devops) or [Jenkins](https://www.jenkins.io/) plus [Octopus Deploy](https://octopus.com/).
