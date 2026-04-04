import { AuthenticatedShell } from "@/features/authenticated-shell/AuthenticatedShell";

export default function AuthenticatedLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <AuthenticatedShell>{children}</AuthenticatedShell>;
}
