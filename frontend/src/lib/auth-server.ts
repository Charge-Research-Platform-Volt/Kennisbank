import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';

const API_URL: string = "http://localhost:8080";
const UNAUTHORIZED_REDIRECT: string = "/unauthorized";
const UNAUTHENTICATED_REDIRECT: string = "/login";

interface RoleResponse 
{
    role: string;
    isAuthenticated: boolean;
}

export async function getCurrentUserRole(): Promise<RoleResponse>
{
    const cookieStore = cookies();

    try
    {
        const response = await fetch(`${API_URL}/roles/current`, {
            headers: {
                Cookie: (await cookieStore).toString(),
            },
            cache: 'no-store'
        });

        if (!response.ok)
            return { role: '', isAuthenticated: false };

        return response.json();
    }
    catch (error)
    {
        console.error('Failed to fetch user role:', error);
        return { role: '', isAuthenticated: false };
    }
}

export async function requireAuthentication(): Promise<void>
{
    const { isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);
}

export async function requireRoleWithRedirect(requiredRole: string, redirectPath: string = UNAUTHORIZED_REDIRECT): Promise<void>
{
    const { role, isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    if (role !== requiredRole)
        redirect(redirectPath);
}

export async function requireAnyRoleWithRedirect(allowedRoles: string[], redirectPath: string = UNAUTHORIZED_REDIRECT): Promise<void>
{
    const { role, isAuthenticated} = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    if (!allowedRoles.includes(role))
        redirect(redirectPath);
}

export async function requireRole(requiredRole: string): Promise<boolean>
{
    const { role, isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    return role === requiredRole;
}

export async function requireAnyRole(allowedRoles: string[]): Promise<boolean>
{
    const { role, isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    return allowedRoles.includes(role);
}