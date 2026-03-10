# 🏋️ Gym Management System

Hệ thống **Gym Management System** giúp quản lý hoạt động của phòng gym như quản lý hội viên, gói tập, huấn luyện viên, lịch tập và thanh toán.

## 📌 Features

### 👤 Member Management

* Thêm / sửa / xóa hội viên
* Quản lý thông tin cá nhân
* Theo dõi trạng thái membership
* Lịch sử đăng ký gói tập

### 💳 Membership Packages

* Tạo các gói tập (1 tháng, 3 tháng, 6 tháng, 1 năm)
* Quản lý giá và thời hạn gói
* Gia hạn gói tập

### 🧑‍🏫 Trainer Management

* Quản lý huấn luyện viên
* Phân công trainer cho hội viên
* Quản lý lịch tập cá nhân

### 💰 Payment Management

* Quản lý thanh toán
* Lịch sử giao dịch
* Báo cáo doanh thu

---

# 🏗️ System Architecture

```
Client (Angular / Web)
        |
        v
Backend API (.NET)
        |
        v
Database (MySQL)
```

---

# ⚙️ Tech Stack

### Frontend

* Angular
* HTML / CSS / SCSS
* DevExtreme / Bootstrap

### Backend

* ASP.NET Core
* RESTful API
* JWT Authentication

### Database

* MySQL

### Other

* Docker
* AWS Deployment

---

# 📂 Project Structure

```
gym-management
│
├── frontend
│   ├── src
│   ├── components
│   ├── services
│
├── backend
│   ├── controllers
│   ├── services
│   ├── repositories
│   ├── entities
│
├── database
│   ├── migrations
│   ├── seed-data
│
└── docs
    ├── api-docs
```

---

# 🚀 Getting Started

## 1️⃣ Clone project

```bash
git clone https://github.com/your-repo/gym-management.git
cd gym-management
```

---

## 2️⃣ Run Backend

```bash
dotnet run
```

---

## 3️⃣ Run Frontend

```bash
cd frontend
npm install
npm start
```

Frontend sẽ chạy tại

```
http://localhost:4200
```

---

# 🔐 Default Account

| Role    | Username | Password   |
| ------- | -------- | ---------- |
| Admin   | admin    | Matkhau2@   |

---

# 🧑‍💻 Contributors

* Developer: Lộc Nguyễn

---
