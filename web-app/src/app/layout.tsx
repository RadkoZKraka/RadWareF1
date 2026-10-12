import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Script from "next/script";
import "./globals.css";

const themeInitializationScript = `
(() => {
  const storageKey = "f1shots-theme";
  const root = document.documentElement;
  const systemQuery = window.matchMedia("(prefers-color-scheme: dark)");
  const savedTheme = localStorage.getItem(storageKey) ?? "system";
  const resolvedTheme = savedTheme === "system"
    ? (systemQuery.matches ? "dark" : "light")
    : savedTheme;

  root.dataset.theme = savedTheme;
  root.dataset.resolvedTheme = resolvedTheme;
})();`;

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "F1Shots Race Club",
  description:
    "Premium Formula 1 prediction leagues, race-day picks, and private competition rooms.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full bg-background text-foreground">
        <Script
          id="theme-initialization"
          strategy="beforeInteractive"
          dangerouslySetInnerHTML={{ __html: themeInitializationScript }}
        />
        {children}
      </body>
    </html>
  );
}
