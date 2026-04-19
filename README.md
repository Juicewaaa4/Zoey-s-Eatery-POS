# Zoey's Eatery POS & Inventory System

A robust, offline-first Point of Sale (POS) and Inventory Management desktop application tailored for an Eatery and Store setting. Built using C# Windows Forms (.NET 8) with a SQLite local database.

## 🚀 Features
- **Standalone Offline Access:** Fast, local SQLite database without the need for constant internet.
- **Network Synchronization:** Supports Windows File Sharing over a Local Area Network (LAN) for simultaneous real-time multi-PC cashflow between Admin and Cashier.
- **Role-Based Access Control (RBAC):** Distinct interfaces and privileges for Administrators and Cashiers.
- **Dynamic POS Dashboard:** Quick tap interface grouped by category with F-key keyboard shortcuts for rapid checkouts.
- **Email Login Notifications:** Sends real-time email alerts to the owner whenever a user logs into the system (powered by Resend API).
- **Stock Tracking & Alerts:** Seamless automated deductions per sale, stock history (In/Out records), and low-stock visual notifications.
- **Comprehensive Reporting:** Export daily sales, fast-moving products, and audit logs to Excel and PDF formats.

## 🛠 Tech Stack
- **Framework:** .NET 8.0 (Windows Forms)
- **Database:** SQLite
- **Charts & Reporting:** ScottPlot, QuestPDF, ClosedXML
- **Email Service:** Resend Email API

## 📦 Deployment
The application is built to be "Self-Contained". You can copy the generated folder containing `TransFundInventory.exe` to any Windows machine and it will run flawlessly without requiring .NET framework installations.

1. Publish via terminal: `dotnet publish -c Release -r win-x64 --self-contained true`
2. Run `TransFundInventory.exe` directly on the host machine.
3. Share the folder on the Network to connect Cashier terminals.


