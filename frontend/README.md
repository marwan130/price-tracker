# Smart Price Tracker — Frontend (React + Vite)

This is the frontend for the Smart Price Tracker platform, built using React, TypeScript, Vite, and Tailwind CSS v4.

## Tech Stack
- **Framework**: React 19 + Vite 8
- **Language**: TypeScript
- **Styling**: Tailwind CSS v4 (using the `@tailwindcss/vite` plugin)
- **Animations**: Framer Motion & GSAP ScrollTrigger
- **Charts**: Recharts
- **Icons**: Lucide React
- **State Management**: Zustand
- **Forms**: React Hook Form + Zod
- **API Client**: Axios

## Getting Started

### Prerequisites
- Node.js (v18)
- The ASP.NET Core backend running locally

### Development Setup

1. **Install Dependencies** (from the repo root or inside `/frontend`):
   ```bash
   cd frontend
   npm install --legacy-peer-deps
   ```

2. **Start the Development Server**:
   ```bash
   npm run dev
   ```
   This will boot up the Vite dev server (typically at `http://localhost:5173`).

3. **Production Build**:
   To bundle the application for production:
   ```bash
   npm run build
   ```
   The compiled assets will be written to the `dist/` directory.