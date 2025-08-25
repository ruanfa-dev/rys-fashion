﻿<p align="center">
  <img src="docs/assets/banner.png" alt="Fashion Recommendation System Banner" width="100%">
</p>

# .NET 9 Fashion Recommendation System with Angular Frontends

## 🧵 Overview

This project showcases a modern, full-stack application built with **.NET 9** for the backend, integrated with **Angular-based sales and admin frontends**, and powered by a **visual similarity model** for fashion recommendations. It's designed for scalable microservice architecture and real-time product suggestion workflows in fashion e-commerce platforms.

## 🔧 Key Components

### 🧠 Visual Similarity Recommendation System
- Deep learning model to analyze and compare fashion item images.
- Delivers **real-time visual similarity-based product recommendations**.
- Integrated via RESTful APIs.

### 🛍️ Angular Sales Frontend
- Customer-facing UI for browsing, filtering, and purchasing products.
- Built with Angular 17+.
- Mobile-responsive with image-based recommendation support.

### 🛠️ Angular Admin Panel
- Admin dashboard to manage inventory, upload fashion images, trigger model re-training, and view analytics.
- Role-based access, full CRUD support, real-time syncing.

## 🚀 Features
- Backend in **.NET 9**
- Angular 17+ frontends for sales and admin
- Visual similarity product recommendation engine (Python)
- Modular, microservice-ready architecture
- Cloud-ready REST APIs
- Cross-platform and container-friendly
- Integrated unit and API testing

## ✅ Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/)
- [Node.js](https://nodejs.org/) & [Angular CLI](https://angular.io/cli)
- Visual Studio or JetBrains Rider (for backend)
- VS Code or WebStorm (for frontend)
- Python 3.8+ (for the model)

## 🏁 Getting Started

### 🔙 Backend (.NET 9)

1. Clone the repository:

   ```bash
   git clone https://github.com/qinfa-dev/Rys.Fashion.git
   cd Rys.Eshop/backend
   ```

2. Build and run the backend service:

   ```bash
   dotnet build
   dotnet run
   ```

### 🎨 Frontend (Angular)

#### Sales Frontend

```bash
cd Rys.Eshop/frontend-sales
npm install
ng serve
```

#### Admin Panel

```bash
cd Rys.Eshop/frontend-admin
npm install
ng serve
```

### 🧠 Visual Similarity Model (Python)

1. Navigate to the similarity engine:

   ```bash
   cd Rys.Eshop/similarity-engine
   ```

2. Install dependencies:

   ```bash
   pip install -r requirements.txt
   ```

3. Run training and inference:

   ```bash
   python train_model.py
   python infer_similar.py
   ```

## 🧪 Testing

### Backend

```bash
dotnet test
```

### Frontend (Angular)

```bash
ng test
```

## 🤝 Contributing

Contributions are welcome!  
To contribute:

1. Fork the repository.
2. Create a new branch.
3. Commit and push your changes.
4. Submit a pull request.

## 📄 License

Licensed under the [MIT License](LICENSE).

## 📬 Contact

For questions or collaboration inquiries:  
📧 [ngtphat.dev@gmail.com](mailto:ngtphat.dev@gmail.com)
