import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"


export default function NewPage() 
{
    return (
        <Tabs defaultValue="resource" className="w-full">
            <TabsList>
                <TabsTrigger value="resource">Resource</TabsTrigger>
                <TabsTrigger value="person">Person</TabsTrigger>
                <TabsTrigger value="organisation">Organisation</TabsTrigger>
            </TabsList>
            <TabsContent value="resource">Create resource here.</TabsContent>
            <TabsContent value="person">Create person here.</TabsContent>
            <TabsContent value="organisation">Create organisation here.</TabsContent>
        </Tabs>
    );
}