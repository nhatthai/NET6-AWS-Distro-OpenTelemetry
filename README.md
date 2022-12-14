# Sample OpenTelemetry on EKS
    Sample OpenTelemetry on EKS, using Github Actions for deployment to EKS
    Apply Zipkin and Jaeger on local
    Apply AWS Collector on local
    Apply AWS Distro in AWS

## Goals
+ Using Terraform: create Infrastructure in AWS
+ Deploy services on AWS EKS with Github Actions
+ Message Bus with RabbitMQ and SQS
+ Integrate OpenTelemetry for tracing and metrics between services
    - Using TraceContextPropagator
+ AWS Distro for OpenTelemetry
+ AWS Collector on local

### Usage
+ Update kubeconfig
    ```
    aws eks update-kubeconfig --region ap-southeast-1 --name microservice-eks
    ```

### Apply Terraform
+ Init Infrastructure
    ```
    terraform init
    terraform apply
    ```

+ Terraform will create a role name with aws-load-balancer-controller name
    + It creates a service account(aws-load-balancer-controller)
    + It sets permission
    + It sets Trust relationships(aws/load-balancer-role-trust-policy)

### Issues in Terraform
+ Fail to create ALB
    ```
    │ Error: could not download chart: failed to download "https://aws.github.io/eks-charts/aws-load-balancer-controller-1.4.6.tgz"
    │
    │   with helm_release.alb,
    │   on lb-controller.tf line 35, in resource "helm_release" "alb":
    │   35: resource "helm_release" "alb" {
    │
    ```
    Fixed: Update Helm Repo
    ```
    helm repo update
    ```

### Ingress on EKS
+ Add public subnets into ingress-eks.yml
+ Create Application Load Balancer
    ```
    kubectl apply -f k8s\ingress-eks.yml
    ```

### Usage OpenTelemetry Collector with docker-compose
+ Using OpenTelemetry Collector on local
    ```
    docker-compose up
    ```

### Usage AWS Collector with docker-compose
+ Using AWS-Collector on local
    ```
    docker-compose -f .\docker-compose-aws-collector.yml up
    ```

+ Post ORDER API
    ```
    http://localhost:5089/order

    Data:
    {
        "OrderNumber": "77777",
        "OrderAmount": 999.7
    }
    ```
+ ![Tracing in Cloud Watch](./images/Tracing-AWS-Collector.png)

### AWS Distro for OpenTelemetry
+ AWS Distro for OpenTelemetry (ADOT) prerequisites(https://docs.aws.amazon.com/eks/latest/userguide/adot-reqts.html)
    ```
    kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.8.2/cert-manager.yaml
    ```

+ Apply the necessary permissions for ADOT to your cluster with the command:
    ```
    kubectl apply -f https://amazon-eks.s3.amazonaws.com/docs/addons-otel-permissions.yaml
    ```

+ [Create an IAM OIDC provider and service account for cluster]
(https://docs.aws.amazon.com/eks/latest/userguide/adot-iam.html)
    - Create an IAM OIDC
    - Set Policy for Service Account
        ```
        eksctl create iamserviceaccount \
        --approve \
        --name adot-collector \
        --namespace default \
        --cluster microservice-eks \
        --attach-policy-arn arn:aws:iam::aws:policy/AmazonPrometheusRemoteWriteAccess \
        --attach-policy-arn arn:aws:iam::aws:policy/AWSXrayWriteOnlyAccess \
        --attach-policy-arn arn:aws:iam::aws:policy/CloudWatchAgentServerPolicy
        ```

    if it shows:
    ```
     1 iamserviceaccount (default/adot-collector) was excluded (based on the include/exclude rules)
    ```
    use command and then re-create service account
    ```
    eksctl delete iamserviceaccount --name=adot-collector --cluster=microservice-eks
    ```

+ Check Service Account
    ```
    kubectl describe sa adot-collector -n default
    ```

+ Add Add-On in EKS
    ```
    aws eks create-addon --addon-name adot --cluster-name microservice-eks
    ```

+ Check Add-On
    ```
    aws eks describe-addon --addon-name adot --cluster-name microservice-eks
    ```

+ Deploy ADOT Collector
    ```
    kubectl apply -f k8s/opentelemetry/collector-config-amp.yml
    kubectl apply -f k8s/opentelemetry/collector-config-cloudwatch.yml
    kubectl apply -f k8s/opentelemetry/collector-config-xray.yml
    ```

### Application Load Balancer
+ Add public subnets into ingress-eks.yml and deploy ingress-eks to EKS

### Post a message from Order API:
    ```
    POST http://k8s-default-ingressw-48da9fc196-431932575.ap-southeast-1.elb.amazonaws.com/order-api/order
    {
        "OrderNumber": "11111",
        "OrderAmount": 1.1
    }
    ```

### Results
+ ![Grafana](./images/grafana.png)
+ ![Trace](./images/trace.png)
+ ![Service Map](./images/service-map.png)
+ ![Trace SQS & SNS](./images/Trace-Map-UsingSNS.png)

### References
+ [Using W3C Trace Context standard in distributed tracing](https://dev.to/luizhlelis/c-using-w3c-trace-context-standard-in-distributed-tracing-1nm0)
+ [Create an ALB Ingress in Amazon EKS](https://aws.amazon.com/premiumsupport/knowledge-center/eks-alb-ingress-aws-waf/)
+ [AWS Load Balancer Controller](https://docs.aws.amazon.com/eks/latest/userguide/aws-load-balancer-controller.html)
+ [AddOn EKS for AWS Distro for OpenTelemetry](https://aws.amazon.com/blogs/containers/metrics-and-traces-collection-using-amazon-eks-add-ons-for-aws-distro-for-opentelemetry/)
+ [ADOT EKS AddOn](https://aws-otel.github.io/docs/getting-started/adot-eks-add-on/installation#deploy-the-adot-collector)
+ [AWS Observability](https://github.com/aws-observability/aws-otel-community)