import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { AUTH_TOKEN_COOKIE_NAME } from "@/features/auth/auth-constants";

export default async function Home() {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(AUTH_TOKEN_COOKIE_NAME)?.value;

  redirect(accessToken ? "/financial-accounts" : "/login");
}
