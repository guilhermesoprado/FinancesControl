import type { Metadata } from "next";
import { Geist } from "next/font/google";
import { AuthProvider } from "@/features/auth/AuthProvider";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-sans",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "FinanceManager",
  description: "Controle financeiro com foco em clareza, previsao e operacao.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="pt-BR" className={geistSans.variable}>
      <body>
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
