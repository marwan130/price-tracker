import { BrowserRouter, Routes, Route } from "react-router-dom";
import { Layout } from "@/components/layout/Layout";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={
            <div className="flex flex-col flex-1 items-center justify-center p-24 text-center">
              <h1 className="text-5xl font-display font-bold text-primary mb-4 animate-pulse-slow">
                Smart Price Tracker
              </h1>
              <p className="text-xl text-text-secondary max-w-md font-sans">
                visual assets successfully initialized using React, Vite, and Tailwind.
              </p>
            </div>
          } />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}