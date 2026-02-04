# Goal
Provide a University Room Booking Platform for students and staff to reserve rooms.

# Users / Actors
- Student
- Faculty
- Admin

# Core Features
- Search availability by building, capacity, and time
- Book and cancel rooms
- Admin approval workflow
- Manage rooms and schedules
- Notifications for booking status
- Audit logging

# Integrations
- SMTP email service
- Campus SSO (OAuth/JWT)
- PostgreSQL

# Data
- User
- Room
- Booking
- Approval
- AuditLog

# Constraints
- Must support JWT auth
- Provide Swagger documentation
- Role-based access control (RBAC)

# Non-Functional Requirements
- Availability: 99.5% uptime
- Security: TLS, JWT, RBAC
- Observability: structured logs and metrics
- Performance: p95 < 500ms

# Key Flows
- Book a room with admin approval
