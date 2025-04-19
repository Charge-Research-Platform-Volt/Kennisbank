import { redirect } from 'next/navigation';
import { FetchWithValidation } from './fetchWithValidation';
import { RoleResponse, RoleResponseSchema } from '@/types/authorization.type';

const API_URL: string = "http://backend:8080";
const UNAUTHORIZED_REDIRECT: string = "/unauthorized";
const UNAUTHENTICATED_REDIRECT: string = "/login";

export async function getCurrentUserRole(): Promise<RoleResponse>
{
    try
    {
        const result = await FetchWithValidation<RoleResponse>(RoleResponseSchema, `${API_URL}/roles/current`);

        if (!result.success)
            return { role: '', isAuthenticated: false };

        return result.data;
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


